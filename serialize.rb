require 'rexml/document'
require 'rexml/element'

module Cpg; end

module Cpg::Serialize
	module MixinClassMethods
		def included_by_module(base)
			if not base.instance_variables.include?('@class_method_module')
				base.instance_variable_set('@class_method_module', true)
				base.extend(MixinClassMethods)
			end
		end
		
		def included(base)
			case base
				when Class
					base.extend(ClassMethods)
				when Module
					included_by_module(base)
			end
		end
	end
	
	extend(MixinClassMethods)
	
	def read_only?(name)
		properties = self.class.read_only || []
		properties.include?(name.to_sym)
	end
	
	def invisible?(name)
		invisible = self.class.invisible || []
		invisible.include?(name.to_sym)
	end
	
	def has_property?(name)
		properties.include?(name.to_sym)
	end
	
	def property_set?(name)
		instance_variable_defined?("@#{name}")
	end
	
	def numeric(name, v)
		return v if name.to_sym == :id
		return v.to_i if v =~ /^\s*-?[0-9]+\s*$/
		return v.to_f if v =~ /^\s*-?[0-9.]+\s*$/

		v
	end
	
	def set_property(name, value)
		value = numeric(name, value)

		if get_property(name) != value
			instance_variable_set("@#{name}", value)
			true
		else
			false
		end
	end
	
	def get_property(name)
		instance_variable_get("@#{name}")
	end
	
	def method_missing(name, *args)
		get_property(name.to_sym)
	end
	
	def properties
		(self.class.properties || [])
	end
	
	def save_properties
		properties
	end
	
	def to_xml
		tagname = self.class.to_s.gsub(/(\w)([A-Z])/) {"#{$1}-#{$2}"}.downcase.gsub(/.*::/, '')
	
		elem = REXML::Element.new(tagname)

		if properties.include?(:id)
			elem.add_attribute('id', get_property(:id).to_s)
		end
	
		# Get defined properties
		save_properties.each do |name|
			next if name == :id
			next unless property_set?(name)

			val = get_property(name)
			
			if val.is_a?(Cpg::Serialize)
				if val.properties.include?(:id)
					elem.add_element('property', {'name' => name.to_s, 'ref' => val.get_property(:id).to_s})
				else
					el = elem.add_element('property', {'name' => name.to_s})
					el << val.to_xml
				end
				
				next
			end
		
			if val
				elem.add_element('property', {'name' => name.to_s, 'value' => val.to_s})
			end
		end
	
		# Get children if object supports it
		if self.is_a?(Enumerable)
			each do |item|
				elem << item.to_xml if item.is_a?(Cpg::Serialize)
			end
		end
	
		elem
	end
	
	def from_xml(element, loaded)
		if element.attributes.include?('id')
			set_property(:id, element.attributes['id'])
		end

		if is_a?(Enumerable)
			# Handle children
			element.elements.each do |elem|
				next if elem.name == 'property'
				
				if elem.is_a?(REXML::Element)
					loaded.unshift(loaded.first.dup)
					obj = Cpg::Serialize.from_xml(elem)
					loaded.shift
					
					if obj.properties.include?(:id) && obj.property_set?(:id)
						loaded.first[obj.get_property(:id)] = obj
					end
					
					self << obj
				end
			end
		end

		# handle properties
		element.elements.each("property") do |elem|
			next unless elem.attributes.include?('name')
			prop = elem.attributes['name'].to_sym
			
			next if prop == :id
			next unless has_property?(prop)

			if elem.attributes.include?('value')
				v = elem.attributes['value']
				set_property(prop, v.empty? ? nil : v)
			elsif elem.elements.size == 1
				set_property(prop, Cpg::Serialize.from_xml(elem.elements[1]))
			elsif elem.attributes.include?('ref') && loaded.first.include?(elem.attributes['ref'])
				set_property(prop, loaded.first[elem.attributes['ref']])
			end
		end
	end		
	
	def self.from_xml_name(name)
		name.gsub(/(-|^)([a-z])/) { $2.upcase }
	end

	def self.from_xml(element)
		klass = from_xml_name(element.name)
		c = nil

		begin
			s = [Cpg::Components, Cpg, Object].find do |space|
				next unless space.const_defined?(klass)

				c = space.const_get(klass)
				c && c.included_modules.include?(Cpg::Serialize)
			end
			
			c = s.const_get(klass) if s
		rescue
			puts "Error in from_xml: #{$!}\n\t#{$@.join("\n\t")}"
			return nil
		end
		
		return nil if c == nil
		
		obj = c.new
		
		@loaded_objects ||= [{}]
		
		obj.from_xml(element, @loaded_objects)

		# handle references
		if obj.properties.include?(:id) && obj.property_set?(:id)
			@loaded_objects.first[obj.get_property(:id)] = obj
		end
		
		obj
	end
	
	def self.reset
		@loaded_objects = [{}]
	end

	module ClassMethods
		attr_reader :properties, :propindex
	
		def inherited(subclass)
			super
			@propindex ||= {}
			@properties ||= []
			@read_only ||= []
			@invisible ||= []
			
			subclass.property *@properties
			subclass.invisible *@invisible
			subclass.read_only *@read_only
		end
	
		def property(*args)
			@properties ||= []
			@propindex ||= {}
			
			args.each { |x| @propindex[x.to_sym] = true }
			@properties += args.collect { |x| x.to_sym }
		end
		
		def read_only(*args)
			@read_only ||= []
			@read_only += args.collect { |x| x.to_sym }
		end
		
		def invisible(*args)
			@invisible ||= []
			@invisible += args.collect { |x| x.to_sym }
		end
	end
end

module Cpg::Serialize::Dynamic
	include Cpg::Serialize

	def ensure_property(name)
		@__propindex ||= {}
		@__properties ||= []
		
		unless (@__propindex.include?(name.to_sym) || self.class.propindex.include?(name.to_sym))
			add_property(name)
		end
	end
	
	def add_property(name)
		@__propindex[name.to_sym] = true
		@__properties << name.to_sym
	end
	
	def has_property?(name)
		# has all properties
		true
	end
	
	def dynamic_properties
		@__properties || []
	end
	
	def dynamic_property?(name)
		(@__propindex || {}).include?(name.to_sym)
	end
	
	def remove_property(name)
		if dynamic_property?(name)
			@__properties.delete(name.to_sym)
			@__propindex.delete(name.to_sym)

			instance_variable_set("@#{name}", nil)

			true
		else
			false
		end
	end
	
	def properties
		# return not only class properties, but also object properties
		self.class.properties + dynamic_properties
	end
	
	def property_set?(name)
		true
	end
	
	def set_property(name, value)
		ensure_property(name)
		super
	end
	
	def get_property(name)
		super || ''
	end
end

# ex:ts=4:noet:

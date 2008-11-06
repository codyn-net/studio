module Cpg; end
module Cpg::Serialize; end

module Cpg::Serialize::Object
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
				
					ClassMethods.initialize(base)
				when Module
					included_by_module(base)
			end
		end
	end

	extend(MixinClassMethods)

	def initialize(*args)
		super(*args)
	
		@properties = {}
	
		class_properties.each { |p| @properties[p] = nil }
	end

	def read_only?(name)
		self.class.read_only.include?(name)
	end

	def invisible?(name)
		self.class.invisible.include?(name)
	end

	def has_property?(name)
		properties.include?(name)
	end

	def property_set?(name)
		properties[name] != nil
	end

	def numeric(name, v)
		return v.to_s if name == 'id'
		return v.to_i if v =~ /^\s*-?[0-9]+\s*$/
		return v.to_f if v =~ /^\s*-?[0-9.]+\s*$/

		v
	end
	
	def merge!(lst)
		lst.each { |k, v| set_property(k, v) }
		self
	end

	def set_property(name, value)
		value = numeric(name, value)

		if get_property(name) != value
			@properties[name] = value
			true
		else
			false
		end
	end

	def to_s
		get_property('id').to_s
	end

	def get_property(name)
		properties[name]
	end

	def add_property(name)
		return false if has_property?(name)
		@properties[name] = nil
	
		true
	end

	def remove_property(name)
		if class_property?(name)
			set_property(name, nil)
		else
			@properties.delete(name)
		end
	end

	def class_property?(name)
		class_properties.include?(name)
	end

	def class_properties
		self.class.properties
	end

	def properties
		@properties
	end

	def save_properties
		properties.keys
	end

	def to_xml
		tagname = self.class.to_s.gsub(/(\w)([A-Z])/) {"#{$1}-#{$2}"}.downcase.gsub(/.*::/, '')

		elem = REXML::Element.new(tagname)

		if properties.include?('id')
			elem.add_attribute('id', get_property('id').to_s)
		end
	
		# Get defined properties
		save_properties.each do |name|
			next if name == 'id'
			next unless property_set?(name)

			val = get_property(name)
		
			if val.is_a?(Cpg::Serialize::Object)
				if val.properties.include?('id')
					elem.add_element('property', {'name' => name.to_s, 'ref' => val.get_property('id').to_s})
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
				elem << item.to_xml if item.is_a?(Cpg::Serialize::Object)
			end
		end

		elem
	end

	def from_xml(element, loaded)
		if element.attributes.include?('id')
			set_property('id', element.attributes['id'])
		end

		if is_a?(Enumerable)
			# Handle children
			element.elements.each do |elem|
				next if elem.name == 'property'
			
				if elem.is_a?(REXML::Element)
					loaded.unshift(loaded.first.dup)
					obj = Cpg::Serialize.from_xml(elem)
					loaded.shift
				
					if obj.property_set?('id')
						loaded.first[obj.get_property('id')] = obj
					end
				
					self << obj
				end
			end
		end

		# handle properties
		element.elements.each("property") do |elem|
			next unless elem.attributes.include?('name')
			prop = elem.attributes['name']

			# skip id property, its special			
			next if prop == 'id'

			if elem.attributes.include?('value')
				v = elem.attributes['value']
				set_property(prop, v)
			elsif elem.elements.size == 1
				set_property(prop, Cpg::Serialize.from_xml(elem.elements[1]))
			elsif elem.attributes.include?('ref') && loaded.first.include?(elem.attributes['ref'])
				set_property(prop, loaded.first[elem.attributes['ref']])
			end
		end
	end

	module ClassMethods
		attr_reader :properties, :invisible, :read_only
	
		def self.initialize(base)
			base.module_eval do
				@properties ||= []
				@read_only ||= []
				@invisible ||= []
			end
		end
	
		def inherited(subclass)
			super
		
			ClassMethods.initialize(subclass)

			subclass.property *@properties
			subclass.invisible *@invisible
			subclass.read_only *@read_only
		end

		def property(*args)
			args.each do |x|
				@properties << x.to_s
			
				define_method("#{x.to_s}") { get_property(x.to_s) }
				define_method("#{x.to_s}=") { |val| set_property(x.to_s, val) }
			end
		end
	
		def read_only(*args)
			@read_only += args.collect { |x| x.to_s }
		end
	
		def invisible(*args)
			@invisible += args.collect { |x| x.to_s }
		end
	
		def visible(*args)
			args.each { |x| @invisible.delete(x.to_s) }
		end
	
		def read_write(*args)
			args.each { |x| @read_only.delete(x.to_s) }
		end
	end
end